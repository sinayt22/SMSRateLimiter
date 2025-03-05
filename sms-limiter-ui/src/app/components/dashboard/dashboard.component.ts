// src/app/components/dashboard/dashboard.component.ts
import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MetricsService } from '../../services/metrics.service';
import { Subscription } from 'rxjs';
import { FilterCriteria } from '../../models/filter-criteria';
import { MetricTimeSeries, PhoneNumberMetrics, GlobalMetrics } from '../../models/metrics';

// Import components
import { FilterPanelComponent } from '../filter-panel/filter-panel.component';
import { MetricsChartComponent } from '../metrics-chart/metrics-chart.component';
import { PerNumberMetricsComponent } from '../per-number-metrics/per-number-metrics.component';
import { NavigationComponent } from '../navigation/navigation.component';

// Angular Material imports
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    FilterPanelComponent,
    MetricsChartComponent,
    PerNumberMetricsComponent,
    NavigationComponent,
    MatButtonModule,
    MatIconModule,
    MatCardModule,
    MatProgressBarModule,
    MatSnackBarModule
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit, OnDestroy {
  phoneMetrics: PhoneNumberMetrics[] = [];
  globalMetrics: GlobalMetrics | null = null;
  timeSeriesData: MetricTimeSeries[] = [];
  filterCriteria: FilterCriteria = {};
  isRefreshing = false;
  lastRefreshed: Date | null = null;
  
  private subscriptions: Subscription[] = [];

  constructor(
    private metricsService: MetricsService,
    private changeDetector: ChangeDetectorRef,
    private snackBar: MatSnackBar
  ) { }

  ngOnInit(): void {
    console.log('Dashboard component initialized');
    
    // Subscribe to metrics updates with proper error handling
    this.subscriptions.push(
      this.metricsService.globalMetrics$.subscribe({
        next: metrics => {
          console.log('Received global metrics update', metrics);
          this.globalMetrics = metrics;
          this.lastRefreshed = new Date();
          this.changeDetector.detectChanges();
        },
        error: err => {
          console.error('Error in global metrics subscription', err);
          this.snackBar.open('Error loading global metrics', 'Dismiss', { duration: 3000 });
        }
      })
    );
    
    this.subscriptions.push(
      this.metricsService.phoneMetrics$.subscribe({
        next: metrics => {
          console.log(`Received phone metrics update: ${metrics.length} phones`);
          this.phoneMetrics = metrics;
          this.changeDetector.detectChanges();
        },
        error: err => {
          console.error('Error in phone metrics subscription', err);
          this.snackBar.open('Error loading phone metrics', 'Dismiss', { duration: 3000 });
        }
      })
    );
    
    this.subscriptions.push(
      this.metricsService.timeSeries$.subscribe({
        next: data => {
          console.log(`Received time series update: ${data.length} points`);
          this.timeSeriesData = data;
          this.changeDetector.detectChanges();
        },
        error: err => {
          console.error('Error in time series subscription', err);
          this.snackBar.open('Error loading time series data', 'Dismiss', { duration: 3000 });
        }
      })
    );
    
    this.subscriptions.push(
      this.metricsService.filterCriteria$.subscribe({
        next: criteria => {
          console.log('Received filter criteria update', criteria);
          this.filterCriteria = criteria;
        },
        error: err => {
          console.error('Error in filter criteria subscription', err);
        }
      })
    );
    
    // Initial data load
    this.onRefreshData();
  }

  ngOnDestroy(): void {
    // Clean up subscriptions
    console.log('Dashboard component destroyed, cleaning up subscriptions');
    this.subscriptions.forEach(sub => sub.unsubscribe());
  }

  onFilterChange(criteria: FilterCriteria): void {
    console.log('Filter changed:', criteria);
    this.metricsService.updateFilterCriteria(criteria);
  }

  onRefreshData(): void {
    console.log('Manual refresh triggered');
    this.isRefreshing = true;
    
    // Refresh data and reset flag when complete
    this.metricsService.refreshData();
    
    // Set a timeout to reset the refreshing indicator
    setTimeout(() => {
      this.isRefreshing = false;
      this.changeDetector.detectChanges();
    }, 1000);
  }
  
  getGlobalTokenPercent(): number {
    if (!this.globalMetrics || this.globalMetrics.maxTokens === 0) return 0;
    return (this.globalMetrics.currentTokens / this.globalMetrics.maxTokens) * 100;
  }
  
  getGlobalAcceptPercent(): number {
    if (!this.globalMetrics || this.globalMetrics.totalRequests === 0) return 0;
    return (this.globalMetrics.acceptedRequests / this.globalMetrics.totalRequests) * 100;
  }
}