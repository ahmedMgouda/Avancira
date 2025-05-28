import { ComponentFixture, TestBed } from '@angular/core/testing';

import { VideoCallWindowComponent } from './video-call-window.component';

describe('VideoCallWindowComponent', () => {
  let component: VideoCallWindowComponent;
  let fixture: ComponentFixture<VideoCallWindowComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [VideoCallWindowComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(VideoCallWindowComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
