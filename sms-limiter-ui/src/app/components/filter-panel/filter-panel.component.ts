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
    MatIconModule
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
  
  private destroy$ = new Subject<void>();

  constructor(
    private fb: FormBuilder,
    private apiService: ApiService
  ) {
    this.filterForm = this.fb.group({
      phoneNumber: [''],
      startDate: [new Date(new Date().getTime() - 3600000)], // Last hour
      endDate: [new Date()],
      refreshInterval: [30000]
    });
  }

  ngOnInit(): void {
    // Set initial form values from input
    if (this.criteria) {
      this.filterForm.patchValue({
        phoneNumber: this.criteria.phoneNumber || '',
        startDate: this.criteria.startDate || new Date(new Date().getTime() - 3600000),
        endDate: this.criteria.endDate || new Date(),
        refreshInterval: this.criteria.refreshInterval || 30000
      });
    }
    
    // React to form changes
    this.filterForm.valueChanges.pipe(
      takeUntil(this.destroy$)
    ).subscribe(values => {
      this.criteriaChange.emit(values as FilterCriteria);
    });
    
    // Load phone numbers
    this.loadPhoneNumbers();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
  
  applyFilters(): void {
    this.criteriaChange.emit(this.filterForm.value as FilterCriteria);
  }
  
  resetFilters(): void {
    this.filterForm.patchValue({
      phoneNumber: '',
      startDate: new Date(new Date().getTime() - 3600000),
      endDate: new Date()
    });
    this.applyFilters();
  }
  
  private loadPhoneNumbers(): void {
    this.apiService.getPhoneNumbers().subscribe(
      phoneNumbers => {
        this.phoneNumbers = phoneNumbers;
      },
      error => {
        console.error('Error loading phone numbers', error);
      }
    );
  }
}