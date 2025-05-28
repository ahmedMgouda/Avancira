import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HelpRequestSentComponent } from './help-request-sent.component';

describe('HelpRequestSentComponent', () => {
  let component: HelpRequestSentComponent;
  let fixture: ComponentFixture<HelpRequestSentComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HelpRequestSentComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HelpRequestSentComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
