// src/app/components/per-number-metrics/per-number-metrics.component.ts
import { Component, Input, OnChanges, SimpleChanges, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatSortModule, Sort } from '@angular/material/sort';
import { PhoneNumberMetrics } from '../../models/metrics';

@Component({
  selector: 'app-per-number-metrics',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatSortModule
  ],
  templateUrl: './per-number-metrics.component.html',
  styleUrl: './per-number-metrics.component.scss'
})
export class PerNumberMetricsComponent implements OnChanges {
  @Input() phoneMetrics: PhoneNumberMetrics[] = [];
  
  displayedColumns: string[] = [
    'phoneNumber',
    'totalRequests',
    'acceptedRequests',
    'rejectedRequests',
    'percentAccepted',
    'requestsPerSecond',
    'currentTokens'
  ];
  
  sortedData: PhoneNumberMetrics[] = [];

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['phoneMetrics']) {
      this.sortedData = [...this.phoneMetrics];
      // Default sort by total requests descending
      this.sortData({ active: 'totalRequests', direction: 'desc' });
    }
  }

  sortData(sort: Sort): void {
    const data = [...this.phoneMetrics];
    if (!sort.active || sort.direction === '') {
      this.sortedData = data;
      return;
    }

    this.sortedData = data.sort((a, b) => {
      const isAsc = sort.direction === 'asc';
      switch (sort.active) {
        case 'phoneNumber': return this.compare(a.phoneNumber, b.phoneNumber, isAsc);
        case 'totalRequests': return this.compare(a.totalRequests, b.totalRequests, isAsc);
        case 'acceptedRequests': return this.compare(a.acceptedRequests, b.acceptedRequests, isAsc);
        case 'rejectedRequests': return this.compare(a.rejectedRequests, b.rejectedRequests, isAsc);
        case 'percentAccepted': return this.compare(a.percentAccepted, b.percentAccepted, isAsc);
        case 'requestsPerSecond': return this.compare(a.requestsPerSecond, b.requestsPerSecond, isAsc);
        case 'currentTokens': return this.compare(a.currentTokens, b.currentTokens, isAsc);
        default: return 0;
      }
    });
  }

  compare(a: number | string, b: number | string, isAsc: boolean): number {
    return (a < b ? -1 : 1) * (isAsc ? 1 : -1);
  }

  getTokenPercent(phone: PhoneNumberMetrics): number {
    if (phone.maxTokens === 0) return 0;
    return (phone.currentTokens / phone.maxTokens) * 100;
  }
}