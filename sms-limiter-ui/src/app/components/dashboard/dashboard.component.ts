// src/app/components/dashboard/dashboard.component.ts
import { Component, OnInit, OnDestroy } from '@angular/core';
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
    MatProgressBarModule
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit, OnDestroy {
  phoneMetrics: PhoneNumberMetrics[] = [];
  globalMetrics: GlobalMetrics | null = null;
  timeSeriesData: MetricTimeSeries[] = [];
  filterCriteria: FilterCriteria = {};
  
  private subscriptions: Subscription[] = [];

  constructor(private metricsService: MetricsService) { }

  ngOnInit(): void {
    // Subscribe to metrics updates
    this.subscriptions.push(
      this.metricsService.globalMetrics$.subscribe(metrics => {
        this.globalMetrics = metrics;
      })
    );
    
    this.subscriptions.push(
      this.metricsService.phoneMetrics$.subscribe(metrics => {
        this.phoneMetrics = metrics;
      })
    );
    
    this.subscriptions.push(
      this.metricsService.timeSeries$.subscribe(data => {
        this.timeSeriesData = data;
      })
    );
    
    this.subscriptions.push(
      this.metricsService.filterCriteria$.subscribe(criteria => {
        this.filterCriteria = criteria;
      })
    );
    
    // Initial data load
    this.metricsService.refreshData();
  }

  ngOnDestroy(): void {
    // Clean up subscriptions
    this.subscriptions.forEach(sub => sub.unsubscribe());
  }

  onFilterChange(criteria: FilterCriteria): void {
    this.metricsService.updateFilterCriteria(criteria);
  }

  onRefreshData(): void {
    this.metricsService.refreshData();
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