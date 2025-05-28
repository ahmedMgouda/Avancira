import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HelpFindATutorComponent } from './help-find-a-tutor.component';

describe('HelpFindATutorComponent', () => {
  let component: HelpFindATutorComponent;
  let fixture: ComponentFixture<HelpFindATutorComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HelpFindATutorComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HelpFindATutorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
