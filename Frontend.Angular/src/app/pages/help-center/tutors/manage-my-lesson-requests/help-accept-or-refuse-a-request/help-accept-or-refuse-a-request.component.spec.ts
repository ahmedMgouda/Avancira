import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HelpAcceptOrRefuseARequestComponent } from './help-accept-or-refuse-a-request.component';

describe('HelpAcceptOrRefuseARequestComponent', () => {
  let component: HelpAcceptOrRefuseARequestComponent;
  let fixture: ComponentFixture<HelpAcceptOrRefuseARequestComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HelpAcceptOrRefuseARequestComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HelpAcceptOrRefuseARequestComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
