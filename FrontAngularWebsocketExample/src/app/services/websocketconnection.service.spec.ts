import { TestBed } from '@angular/core/testing';

import { WebsocketconnectionService } from './websocketconnection.service';

describe('WebsocketconnectionService', () => {
  let service: WebsocketconnectionService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(WebsocketconnectionService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
