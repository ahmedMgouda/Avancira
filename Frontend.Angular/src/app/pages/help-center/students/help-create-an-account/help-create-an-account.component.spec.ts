import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HelpCreateAnAccountComponent } from './help-create-an-account.component';

describe('HelpCreateAnAccountComponent', () => {
  let component: HelpCreateAnAccountComponent;
  let fixture: ComponentFixture<HelpCreateAnAccountComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HelpCreateAnAccountComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HelpCreateAnAccountComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
