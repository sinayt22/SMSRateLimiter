// src/app/components/filter-panel/filter-panel.component.ts
import { Component, EventEmitter, Input, OnDestroy, OnInit, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Subject, interval, takeUntil, merge } from 'rxjs';
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
  isLoadingPhoneNumbers = false;
  lastPhoneNumbersUpdate = new Date();
  
  private destroy$ = new Subject<void>();
  private phoneNumberRefreshInterval = 30000; // 30 seconds

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
    
    // Watch for refresh interval changes to update phone numbers refresh rate
    this.filterForm.get('refreshInterval')?.valueChanges.pipe(
      takeUntil(this.destroy$)
    ).subscribe(value => {
      // Update phone numbers refresh interval based on data refresh interval
      // (but don't make it shorter than 30 seconds to avoid too many API calls)
      if (value && value > 0) {
        this.phoneNumberRefreshInterval = Math.max(30000, value);
      } else {
        // If auto-refresh is disabled, still check for new numbers every 2 minutes
        this.phoneNumberRefreshInterval = 120000;
      }
    });
    
    // Initial phone numbers load
    this.loadPhoneNumbers();
    
    // Set up periodic refresh of phone numbers list
    merge(
      // Refresh on a regular interval
      interval(this.phoneNumberRefreshInterval),
      // Also refresh when the form's refresh interval changes
      this.filterForm.get('refreshInterval')?.valueChanges || []
    )
    .pipe(takeUntil(this.destroy$))
    .subscribe(() => {
      this.loadPhoneNumbers();
    });
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
  
  refreshPhoneNumbers(): void {
    this.loadPhoneNumbers(true);
  }
  
  private loadPhoneNumbers(forceRefresh = false): void {
    // Avoid too frequent refreshes unless forced
    const now = new Date();
    const timeSinceLastUpdate = now.getTime() - this.lastPhoneNumbersUpdate.getTime();
    
    if (!forceRefresh && timeSinceLastUpdate < 5000) {
      console.log('Skipping phone numbers refresh - too soon since last update');
      return;
    }
    
    this.isLoadingPhoneNumbers = true;
    
    this.apiService.getPhoneNumbers().subscribe({
      next: phoneNumbers => {
        // Only update if there are changes
        if (JSON.stringify(this.phoneNumbers) !== JSON.stringify(phoneNumbers)) {
          console.log(`Phone numbers list updated: ${phoneNumbers.length} numbers available`);
          
          // Get the currently selected value
          const currentValue = this.filterForm.get('phoneNumber')?.value;
          
          // Sort the phone numbers for better UX
          this.phoneNumbers = phoneNumbers.sort();
          
          // If the currently selected value is no longer in the list, reset to "All"
          if (currentValue && !phoneNumbers.includes(currentValue)) {
            this.filterForm.patchValue({ phoneNumber: '' });
          }
        }
        
        this.isLoadingPhoneNumbers = false;
        this.lastPhoneNumbersUpdate = new Date();
      },
      error: error => {
        console.error('Error loading phone numbers', error);
        this.isLoadingPhoneNumbers = false;
      }
    });
  }
}