import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HelpBookAPackComponent } from './help-book-a-pack.component';

describe('HelpBookAPackComponent', () => {
  let component: HelpBookAPackComponent;
  let fixture: ComponentFixture<HelpBookAPackComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HelpBookAPackComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HelpBookAPackComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
