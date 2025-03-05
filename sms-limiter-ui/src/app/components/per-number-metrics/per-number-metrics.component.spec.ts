import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PerNumberMetricsComponent } from './per-number-metrics.component';

describe('PerNumberMetricsComponent', () => {
  let component: PerNumberMetricsComponent;
  let fixture: ComponentFixture<PerNumberMetricsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PerNumberMetricsComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(PerNumberMetricsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
