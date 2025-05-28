import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HelpWhatIsYourStatusAsATutorComponent } from './help-what-is-your-status-as-a-tutor.component';

describe('HelpWhatIsYourStatusAsATutorComponent', () => {
  let component: HelpWhatIsYourStatusAsATutorComponent;
  let fixture: ComponentFixture<HelpWhatIsYourStatusAsATutorComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HelpWhatIsYourStatusAsATutorComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HelpWhatIsYourStatusAsATutorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
