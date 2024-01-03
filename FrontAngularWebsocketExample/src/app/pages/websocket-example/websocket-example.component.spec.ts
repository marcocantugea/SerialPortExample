import { ComponentFixture, TestBed } from '@angular/core/testing';

import { WebsocketExampleComponent } from './websocket-example.component';

describe('WebsocketExampleComponent', () => {
  let component: WebsocketExampleComponent;
  let fixture: ComponentFixture<WebsocketExampleComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [WebsocketExampleComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(WebsocketExampleComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
