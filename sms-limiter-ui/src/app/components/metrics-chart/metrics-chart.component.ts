// src/app/components/metrics-chart/metrics-chart.component.ts
import { Component, Input, OnChanges, OnInit, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import * as Highcharts from 'highcharts';
import { HighchartsChartModule } from 'highcharts-angular';
import { MetricTimeSeries } from '../../models/metrics';

@Component({
  selector: 'app-metrics-chart',
  standalone: true,
  imports: [
    CommonModule,
    HighchartsChartModule
  ],
  templateUrl: './metrics-chart.component.html',
  styleUrl: './metrics-chart.component.scss'
})
export class MetricsChartComponent implements OnInit, OnChanges {
  @Input() timeSeriesData: MetricTimeSeries[] = [];
  
  Highcharts: typeof Highcharts = Highcharts;
  chartOptions: Highcharts.Options = {};
  updateFlag = false;
  
  constructor() { }

  ngOnInit(): void {
    this.initChart();
  }
  
  ngOnChanges(changes: SimpleChanges): void {
    if (changes['timeSeriesData']) {
      this.updateChartData();
    }
  }
  
  private initChart(): void {
    this.chartOptions = {
      chart: {
        type: 'area',
        zooming: {
          type: 'x'
        }
      },
      title: {
        text: undefined
      },
      credits: {
        enabled: false
      },
      xAxis: {
        type: 'datetime',
        title: {
          text: 'Time'
        },
        dateTimeLabelFormats: {
          millisecond: '%H:%M:%S.%L',
          second: '%H:%M:%S',
          minute: '%H:%M',
          hour: '%H:%M',
          day: '%e. %b',
          week: '%e. %b',
          month: '%b \'%y',
          year: '%Y'
        }
      },
      yAxis: {
        title: {
          text: 'Messages'
        },
        min: 0
      },
      tooltip: {
        shared: true,
        valueDecimals: 0,
        xDateFormat: '%Y-%m-%d %H:%M:%S',
        useHTML: true
      },
      // We'll use a different approach for timezone handling
      // by adjusting the timestamps directly
      plotOptions: {
        area: {
          stacking: 'normal',
          lineColor: '#666666',
          lineWidth: 1,
          marker: {
            lineWidth: 1,
            lineColor: '#666666'
          }
        }
      },
      series: [
        {
          name: 'Rejected',
          data: [],
          type: 'area',
          color: '#f44336'
        },
        {
          name: 'Accepted',
          data: [],
          type: 'area',
          color: '#4caf50'
        }
      ]
    };
    
    this.updateChartData();
  }
  
  private updateChartData(): void {
    if (!this.timeSeriesData || this.timeSeriesData.length === 0) {
      return;
    }
    
    const acceptedData: [number, number][] = [];
    const rejectedData: [number, number][] = [];
    
    this.timeSeriesData.forEach(point => {
      // Apply timezone offset to get local time representation
      // This compensates for Highcharts' UTC default interpretation
      const timestamp = point.timestamp.getTime();
      const timezoneOffset = point.timestamp.getTimezoneOffset() * 60000; // convert minutes to milliseconds
      const adjustedTimestamp = timestamp - timezoneOffset;
      
      acceptedData.push([adjustedTimestamp, point.accepted]);
      rejectedData.push([adjustedTimestamp, point.rejected]);
    });
    
    this.chartOptions = {
      ...this.chartOptions,
      series: [
        {
          name: 'Rejected',
          data: rejectedData,
          type: 'area',
          color: '#f44336'
        },
        {
          name: 'Accepted',
          data: acceptedData,
          type: 'area',
          color: '#4caf50'
        }
      ]
    };
    
    this.updateFlag = true;
  }
}