import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HelpContactATutorComponent } from './help-contact-a-tutor.component';

describe('HelpContactATutorComponent', () => {
  let component: HelpContactATutorComponent;
  let fixture: ComponentFixture<HelpContactATutorComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HelpContactATutorComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HelpContactATutorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
