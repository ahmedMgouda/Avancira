import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HelpCreateAnAdComponent } from './help-create-an-ad.component';

describe('HelpCreateAnAdComponent', () => {
  let component: HelpCreateAnAdComponent;
  let fixture: ComponentFixture<HelpCreateAnAdComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HelpCreateAnAdComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HelpCreateAnAdComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
