import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HelpDeactivateOrReactivateMyAdComponent } from './help-deactivate-or-reactivate-my-ad.component';

describe('HelpDeactivateOrReactivateMyAdComponent', () => {
  let component: HelpDeactivateOrReactivateMyAdComponent;
  let fixture: ComponentFixture<HelpDeactivateOrReactivateMyAdComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HelpDeactivateOrReactivateMyAdComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HelpDeactivateOrReactivateMyAdComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
