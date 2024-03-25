import { Component } from '@angular/core';
import { WebsocketconnectionService } from '../../services/websocketconnection.service';
import { Observable, Subscriber, Subscription } from 'rxjs';

@Component({
  selector: 'app-websocket-example',
  standalone: true,
  imports: [],
  templateUrl: './websocket-example.component.html',
  styleUrl: './websocket-example.component.css'
})
export class WebsocketExampleComponent {

  message: string = "0";
  subscriptions: Subscription[] = [];

  constructor( private websocketService:WebsocketconnectionService ){}
  
  ngOnInit(): void {
    this.OpenWebSocket();
  }

  OpenWebSocket() {
    this.subscriptions.push(this.websocketService.GetConnection().subscribe({
      next: (response) => {

        this.message = response.message;
      },
      error: (error) => {
        console.log(error);
        this.message = error;
      }
    }));
  }

  CloseWebSocket() {
    this.websocketService.CloseConnection();
    this.subscriptions.forEach((s) => { s.unsubscribe });
  }

}
