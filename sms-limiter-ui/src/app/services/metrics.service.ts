// src/app/services/metrics.service.ts
import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, interval, of } from 'rxjs';
import { ApiService } from './api.service';
import { FilterCriteria } from '../models/filter-criteria';
import { MessageMetrics, MetricTimeSeries, PhoneNumberMetrics, GlobalMetrics, BucketStatus } from '../models/metrics';
import { switchMap, map, tap, catchError } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class MetricsService {
  // Observable sources
  private messageMetricsSubject = new BehaviorSubject<MessageMetrics[]>([]);
  private globalMetricsSubject = new BehaviorSubject<GlobalMetrics | null>(null);
  private phoneMetricsSubject = new BehaviorSubject<PhoneNumberMetrics[]>([]);
  private timeSeriesSubject = new BehaviorSubject<MetricTimeSeries[]>([]);
  private filterCriteriaSubject = new BehaviorSubject<FilterCriteria>({
    startDate: new Date(new Date().getTime() - 3600000), // Last hour
    endDate: new Date(),
    refreshInterval: 30000 // 30 seconds
  });
  
  // Observable streams
  readonly messageMetrics$ = this.messageMetricsSubject.asObservable();
  readonly globalMetrics$ = this.globalMetricsSubject.asObservable();
  readonly phoneMetrics$ = this.phoneMetricsSubject.asObservable();
  readonly timeSeries$ = this.timeSeriesSubject.asObservable();
  readonly filterCriteria$ = this.filterCriteriaSubject.asObservable();
  
  private refreshSubscription: any;
  private globalBucketStatus: Partial<BucketStatus> | null = null;

  constructor(private apiService: ApiService) {
    // Start with default refresh
    this.setupRefreshInterval();
  }

  // Update filter criteria and refresh data
  updateFilterCriteria(criteria: Partial<FilterCriteria>): void {
    const currentCriteria = this.filterCriteriaSubject.value;
    const newCriteria = { ...currentCriteria, ...criteria };
    this.filterCriteriaSubject.next(newCriteria);
    
    // If the refresh interval changed, update the timer
    if (criteria.refreshInterval !== undefined && 
        criteria.refreshInterval !== currentCriteria.refreshInterval) {
      this.setupRefreshInterval();
    }
    
    // Immediately refresh data with new criteria
    this.refreshData();
  }

  // Fetch fresh data based on current filter criteria
  refreshData(): void {
    const criteria = this.filterCriteriaSubject.value;
    
    this.apiService.getMessageMetrics(criteria).pipe(
      catchError(error => {
        console.error('Error fetching message metrics', error);
        return of([]);
      })
    ).subscribe(metrics => {
      this.messageMetricsSubject.next(metrics);
      
      // Process the raw metrics into our derived metrics
      this.processGlobalMetrics(metrics);
      this.processPhoneMetrics(metrics);
      this.processTimeSeriesMetrics(metrics);
    });
  }

  // Set up the automatic refresh interval
  private setupRefreshInterval(): void {
    // Clear any existing subscription
    if (this.refreshSubscription) {
      this.refreshSubscription.unsubscribe();
    }
    
    const refreshMs = this.filterCriteriaSubject.value.refreshInterval || 30000;
    
    // Don't set up interval if refresh is disabled (0 or negative)
    if (refreshMs <= 0) return;
    
    this.refreshSubscription = interval(refreshMs).pipe(
      tap(() => this.refreshData())
    ).subscribe();
  }
  
  // Transform raw message metrics into global summary
  private processGlobalMetrics(metrics: MessageMetrics[]): void {
    if (!metrics || metrics.length === 0) {
      this.globalMetricsSubject.next(null);
      return;
    }
    
    const totalRequests = metrics.reduce((sum, metric) => sum + metric.requestCount, 0);
    const acceptedRequests = metrics.reduce((sum, metric) => sum + metric.acceptedCount, 0);
    const rejectedRequests = metrics.reduce((sum, metric) => sum + metric.rejectedCount, 0);
    
    // Calculate time span in seconds for requests per second
    const timestamps = metrics.map(m => m.timestamp.getTime());
    const timeSpan = timestamps.length > 1 
      ? (Math.max(...timestamps) - Math.min(...timestamps)) / 1000
      : 1;
    
    // Use the global bucket status data if available
    this.apiService.getBucketStatus(metrics[0].phoneNumber).pipe(
      catchError(() => of(null))
    ).subscribe(status => {
      if (status) {
        this.globalBucketStatus = {
          globalCurrentTokens: status.globalCurrentTokens,
          globalMaxTokens: status.globalMaxTokens,
          globalRefillRate: status.globalRefillRate,
          globalLastUsed: status.globalLastUsed
        };
      }
      
      const globalMetrics: GlobalMetrics = {
        totalRequests,
        acceptedRequests,
        rejectedRequests,
        percentAccepted: totalRequests > 0 ? (acceptedRequests / totalRequests) * 100 : 0,
        requestsPerSecond: totalRequests / Math.max(1, timeSpan),
        currentTokens: this.globalBucketStatus?.globalCurrentTokens || 0,
        maxTokens: this.globalBucketStatus?.globalMaxTokens || 0
      };
      
      this.globalMetricsSubject.next(globalMetrics);
    });
  }
  
  // Transform raw message metrics into phone number summaries
  private processPhoneMetrics(metrics: MessageMetrics[]): void {
    // Group by phone number
    const phoneMap = new Map<string, {
      requests: number,
      accepted: number,
      rejected: number,
      timestamps: Date[]
    }>();
    
    metrics.forEach(metric => {
      const phoneNumber = metric.phoneNumber;
      
      if (!phoneMap.has(phoneNumber)) {
        phoneMap.set(phoneNumber, {
          requests: 0,
          accepted: 0,
          rejected: 0,
          timestamps: []
        });
      }
      
      const data = phoneMap.get(phoneNumber)!;
      data.requests += metric.requestCount;
      data.accepted += metric.acceptedCount;
      data.rejected += metric.rejectedCount;
      data.timestamps.push(metric.timestamp);
    });
    
    // Create phone metrics with available data (will update with token info later)
    const phoneMetrics: PhoneNumberMetrics[] = Array.from(phoneMap.entries())
      .map(([phoneNumber, data]) => {
        // Calculate time span in seconds for requests per second
        const timeSpan = data.timestamps.length > 1 
          ? (Math.max(...data.timestamps.map(d => d.getTime())) - 
             Math.min(...data.timestamps.map(d => d.getTime()))) / 1000
          : 1;
          
        return {
          phoneNumber,
          totalRequests: data.requests,
          acceptedRequests: data.accepted,
          rejectedRequests: data.rejected,
          percentAccepted: data.requests > 0 ? (data.accepted / data.requests) * 100 : 0,
          requestsPerSecond: data.requests / Math.max(1, timeSpan),
          currentTokens: 0, // Will be updated with actual data
          maxTokens: 0 // Will be updated with actual data
        };
      })
      .sort((a, b) => b.totalRequests - a.totalRequests);
      
    this.phoneMetricsSubject.next(phoneMetrics);
    
    // For the top 10 phone numbers, fetch their current bucket status
    // (limiting to 10 to avoid too many parallel requests)
    phoneMetrics.slice(0, 10).forEach(metric => {
      this.apiService.getBucketStatus(metric.phoneNumber).pipe(
        catchError(error => {
          console.error(`Error fetching bucket status for ${metric.phoneNumber}`, error);
          return of(null);
        })
      ).subscribe(status => {
        if (!status) return;
        
        // Update the phone metrics with current token information
        const updatedMetrics = this.phoneMetricsSubject.value.map(m => {
          if (m.phoneNumber === metric.phoneNumber) {
            return {
              ...m,
              currentTokens: status.phoneNumberCurrentTokens,
              maxTokens: status.phoneNumberMaxTokens
            };
          }
          return m;
        });
        
        this.phoneMetricsSubject.next(updatedMetrics);
      });
    });
  }
  
  // Process time-series data for charts
  private processTimeSeriesMetrics(metrics: MessageMetrics[]): void {
    // Group by timestamp (rounded to the minute for readability)
    const timeMap = new Map<number, {
      requests: number,
      accepted: number,
      rejected: number
    }>();
    
    metrics.forEach(metric => {
      // Round to the minute
      const roundedTime = new Date(metric.timestamp);
      roundedTime.setSeconds(0, 0);
      const timeKey = roundedTime.getTime();
      
      if (!timeMap.has(timeKey)) {
        timeMap.set(timeKey, {
          requests: 0,
          accepted: 0,
          rejected: 0
        });
      }
      
      const data = timeMap.get(timeKey)!;
      data.requests += metric.requestCount;
      data.accepted += metric.acceptedCount;
      data.rejected += metric.rejectedCount;
    });
    
    // Convert to array and sort by timestamp
    const timeSeries: MetricTimeSeries[] = Array.from(timeMap.entries())
      .map(([time, data]) => ({
        timestamp: new Date(time),
        requests: data.requests,
        accepted: data.accepted,
        rejected: data.rejected
      }))
      .sort((a, b) => a.timestamp.getTime() - b.timestamp.getTime());
      
    this.timeSeriesSubject.next(timeSeries);
  }
  
  // Get a single, up-to-date bucket status (not cached)
  getBucketStatus(phoneNumber: string): Observable<PhoneNumberMetrics | null> {
    return this.apiService.getBucketStatus(phoneNumber).pipe(
      map(status => {
        // Find if we have existing metrics for this number
        const existingMetrics = this.phoneMetricsSubject.value.find(
          m => m.phoneNumber === phoneNumber
        );
        
        if (existingMetrics) {
          return {
            ...existingMetrics,
            currentTokens: status.phoneNumberCurrentTokens,
            maxTokens: status.phoneNumberMaxTokens
          };
        }
        
        // Create new metrics object if we don't have data
        return {
          phoneNumber: status.phoneNumber,
          totalRequests: 0,
          acceptedRequests: 0,
          rejectedRequests: 0,
          percentAccepted: 0,
          requestsPerSecond: 0,
          currentTokens: status.phoneNumberCurrentTokens,
          maxTokens: status.phoneNumberMaxTokens
        };
      }),
      catchError(error => {
        console.error(`Error fetching bucket status for ${phoneNumber}`, error);
        return of(null);
      })
    );
  }
}