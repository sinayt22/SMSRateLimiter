// src/app/services/api.service.ts
import { Injectable } from '@angular/core';
import { HttpClient, HttpParams, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { BucketStatus, MessageMetrics } from '../models/metrics';
import { FilterCriteria } from '../models/filter-criteria';
import { map, catchError, retry, timeout } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private baseUrl = 'http://localhost:5139/api';
  private timeoutMs = 10000; // 10 second timeout for requests

  constructor(private http: HttpClient) {
    console.log('ApiService initialized with base URL:', this.baseUrl);
  }

  // Get bucket status for a specific phone number
  getBucketStatus(phoneNumber: string): Observable<BucketStatus> {
    console.log(`Getting bucket status for ${phoneNumber}`);
    return this.http.get<BucketStatus>(`${this.baseUrl}/RateLimiter/status/${phoneNumber}`)
      .pipe(
        timeout(this.timeoutMs),
        retry(1), // Retry once on error
        map(response => {
          // Convert string dates to Date objects
          return {
            ...response,
            phoneNumberLastUsed: new Date(response.phoneNumberLastUsed),
            globalLastUsed: new Date(response.globalLastUsed)
          };
        }),
        catchError(this.handleError)
      );
  }

  // Get message metrics with optional filtering
  getMessageMetrics(criteria: FilterCriteria): Observable<MessageMetrics[]> {
    console.log('Getting message metrics with criteria:', criteria);
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
    
    // Add a cache-busting parameter to prevent browser caching
    params = params.set('_t', Date.now().toString());
    
    console.log(`Calling API: ${this.baseUrl}/Metrics/messages at ${new Date().toISOString()}`);
    
    return this.http.get<MessageMetrics[]>(`${this.baseUrl}/Metrics/messages`, { 
      params,
      headers: {
        'Cache-Control': 'no-cache, no-store, must-revalidate',
        'Pragma': 'no-cache',
        'Expires': '0'
      }
    })
    .pipe(
      timeout(this.timeoutMs),
      retry(1), // Retry once on error
      map(metrics => {
        console.log(`Received ${metrics.length} metrics from API`);
        return metrics.map(metric => ({
          ...metric,
          timestamp: new Date(metric.timestamp)
        }));
      }),
      catchError(error => {
        console.error(`Error fetching metrics at ${new Date().toISOString()}:`, error);
        if (error.name === 'HttpErrorResponse') {
          console.error('Full error object:', JSON.stringify(error, null, 2));
        }
        return this.handleError(error);
      })
    );
  }

  // Get phone numbers list
  getPhoneNumbers(): Observable<string[]> {
    console.log('Getting phone numbers list');
    return this.http.get<string[]>(`${this.baseUrl}/Metrics/phones`)
      .pipe(
        timeout(this.timeoutMs),
        retry(1), // Retry once on error
        catchError(this.handleError)
      );
  }

  // Simulate check rate limit API for testing
  checkRateLimit(phoneNumber: string): Observable<any> {
    console.log(`Checking rate limit for ${phoneNumber}`);
    return this.http.post(`${this.baseUrl}/RateLimiter/check`, { phoneNumber })
      .pipe(
        timeout(this.timeoutMs),
        catchError(this.handleError)
      );
  }
  
  // Clear all metrics (for testing/reset)
  clearMetrics(): Observable<any> {
    console.log('Clearing all metrics');
    return this.http.delete(`${this.baseUrl}/Metrics/clear`)
      .pipe(
        timeout(this.timeoutMs),
        catchError(this.handleError)
      );
  }
  
  // Central error handler
  private handleError(error: HttpErrorResponse) {
    let errorMessage = '';
    
    if (error.error instanceof ErrorEvent) {
      // Client-side error
      errorMessage = `Client error: ${error.error.message}`;
    } else {
      // Server-side error
      errorMessage = `Server error: ${error.status} - ${error.statusText || ''}, Message: ${error.message}`;
    }
    
    console.error(errorMessage);
    return throwError(() => new Error(errorMessage));
  }
}