// src/app/services/api.service.ts
import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { BucketStatus, MessageMetrics } from '../models/metrics';
import { FilterCriteria } from '../models/filter-criteria';
import { map } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private baseUrl = 'http://localhost:5139/api';

  constructor(private http: HttpClient) { }

  // Get bucket status for a specific phone number
  getBucketStatus(phoneNumber: string): Observable<BucketStatus> {
    return this.http.get<BucketStatus>(`${this.baseUrl}/RateLimiter/status/${phoneNumber}`)
      .pipe(
        map(response => {
          // Convert string dates to Date objects
          return {
            ...response,
            phoneNumberLastUsed: new Date(response.phoneNumberLastUsed),
            globalLastUsed: new Date(response.globalLastUsed)
          };
        })
      );
  }

  // Get message metrics with optional filtering
  getMessageMetrics(criteria: FilterCriteria): Observable<MessageMetrics[]> {
    let params = new HttpParams();
    
    if (criteria.phoneNumber) {
      params = params.set('phoneNumber', criteria.phoneNumber);
    }
    
    if (criteria.startDate) {
      params = params.set('startDate', criteria.startDate.toISOString());
    }
    
    if (criteria.endDate) {
      params = params.set('endDate', criteria.endDate.toISOString());
    }
    
    return this.http.get<MessageMetrics[]>(`${this.baseUrl}/Metrics/messages`, { params })
      .pipe(
        map(metrics => metrics.map(metric => ({
          ...metric,
          timestamp: new Date(metric.timestamp)
        })))
      );
  }

  // Get phone numbers list
  getPhoneNumbers(): Observable<string[]> {
    return this.http.get<string[]>(`${this.baseUrl}/Metrics/phones`);
  }

  // Simulate check rate limit API for testing
  checkRateLimit(phoneNumber: string): Observable<any> {
    return this.http.post(`${this.baseUrl}/RateLimiter/check`, { phoneNumber });
  }
}