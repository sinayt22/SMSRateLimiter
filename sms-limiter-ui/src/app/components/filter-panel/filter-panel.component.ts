// src/app/components/filter-panel/filter-panel.component.ts
import { Component, EventEmitter, Input, OnDestroy, OnInit, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Subject, takeUntil } from 'rxjs';
import { FilterCriteria } from '../../models/filter-criteria';
import { ApiService } from '../../services/api.service';

// Angular Material imports
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatTooltipModule } from '@angular/material/tooltip';

@Component({
  selector: 'app-filter-panel',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatButtonModule,
    MatIconModule,
    MatButtonToggleModule,
    MatTooltipModule
  ],
  templateUrl: './filter-panel.component.html',
  styleUrl: './filter-panel.component.scss'
})
export class FilterPanelComponent implements OnInit, OnDestroy {
  @Input() criteria: FilterCriteria = {};
  
  @Output() criteriaChange = new EventEmitter<FilterCriteria>();
  
  filterForm!: FormGroup;
  phoneNumbers: string[] = [];
  refreshIntervals = [
    { value: 0, label: 'Off' },
    { value: 5000, label: '5 seconds' },
    { value: 15000, label: '15 seconds' },
    { value: 30000, label: '30 seconds' },
    { value: 60000, label: '1 minute' },
    { value: 300000, label: '5 minutes' }
  ];
  
  timeRanges = [
    { value: 'last-hour', label: 'Last Hour' },
    { value: 'last-6-hours', label: 'Last 6 Hours' },
    { value: 'last-day', label: 'Last 24 Hours' },
    { value: 'last-week', label: 'Last 7 Days' },
    { value: 'custom', label: 'Custom Range' }
  ];
  
  showCustomDateRange = false;
  
  private destroy$ = new Subject<void>();

  constructor(
    private fb: FormBuilder,
    private apiService: ApiService
  ) {
    // Default to one hour time range
    const now = new Date();
    const oneHourAgo = new Date(now.getTime() - 3600000);
    
    this.filterForm = this.fb.group({
      phoneNumber: [''],
      timeRange: ['last-hour'],
      startDate: [oneHourAgo],
      endDate: [now],
      refreshInterval: [30000]
    });
  }

  ngOnInit(): void {
    // Set initial form values from input
    if (this.criteria) {
      // Determine if we have a custom time range
      let timeRange = 'last-hour';
      if (this.criteria.startDate && this.criteria.endDate) {
        const now = new Date();
        const diffHours = (now.getTime() - this.criteria.startDate.getTime()) / (1000 * 60 * 60);
        
        if (Math.abs(diffHours - 1) < 0.1) {
          timeRange = 'last-hour';
        } else if (Math.abs(diffHours - 6) < 0.1) {
          timeRange = 'last-6-hours';
        } else if (Math.abs(diffHours - 24) < 0.1) {
          timeRange = 'last-day';
        } else if (Math.abs(diffHours - 168) < 0.1) {
          timeRange = 'last-week';
        } else {
          timeRange = 'custom';
          this.showCustomDateRange = true;
        }
      }
      
      this.filterForm.patchValue({
        phoneNumber: this.criteria.phoneNumber || '',
        timeRange: timeRange,
        startDate: this.criteria.startDate || new Date(new Date().getTime() - 3600000),
        endDate: this.criteria.endDate || new Date(),
        refreshInterval: this.criteria.refreshInterval || 30000
      });
    }
    
    // React to form changes
    this.filterForm.get('timeRange')?.valueChanges.pipe(
      takeUntil(this.destroy$)
    ).subscribe(value => {
      this.showCustomDateRange = value === 'custom';
      
      if (value !== 'custom') {
        // Set the date range based on selected preset
        const now = new Date();
        let startDate: Date;
        
        switch (value) {
          case 'last-hour':
            startDate = new Date(now.getTime() - 3600000); // 1 hour
            break;
          case 'last-6-hours':
            startDate = new Date(now.getTime() - 21600000); // 6 hours
            break;
          case 'last-day':
            startDate = new Date(now.getTime() - 86400000); // 24 hours
            break;
          case 'last-week':
            startDate = new Date(now.getTime() - 604800000); // 7 days
            break;
          default:
            startDate = new Date(now.getTime() - 3600000); // Default to 1 hour
        }
        
        this.filterForm.patchValue({
          startDate: startDate,
          endDate: now
        });
      }
    });
    
    // Load phone numbers
    this.loadPhoneNumbers();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
  
  applyFilters(): void {
    const formValues = this.filterForm.value;
    
    // Build the criteria object
    const criteria: FilterCriteria = {
      phoneNumber: formValues.phoneNumber,
      startDate: formValues.startDate,
      endDate: formValues.endDate,
      refreshInterval: formValues.refreshInterval
    };
    
    console.log('Applying filters:', criteria);
    this.criteriaChange.emit(criteria);
  }
  
  resetFilters(): void {
    const now = new Date();
    const oneHourAgo = new Date(now.getTime() - 3600000);
    
    this.filterForm.patchValue({
      phoneNumber: '',
      timeRange: 'last-hour',
      startDate: oneHourAgo,
      endDate: now
    });
    
    this.applyFilters();
  }
  
  private loadPhoneNumbers(): void {
    this.apiService.getPhoneNumbers().subscribe({
      next: phoneNumbers => {
        this.phoneNumbers = phoneNumbers;
      },
      error: error => {
        console.error('Error loading phone numbers', error);
      }
    });
  }
}