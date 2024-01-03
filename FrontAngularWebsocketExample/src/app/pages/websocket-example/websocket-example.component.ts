import { Component } from '@angular/core';
import { WebsocketconnectionService } from '../../services/websocketconnection.service';

@Component({
  selector: 'app-websocket-example',
  standalone: true,
  imports: [],
  templateUrl: './websocket-example.component.html',
  styleUrl: './websocket-example.component.css'
})
export class WebsocketExampleComponent {

  message:string="0";

  constructor( private websocketService:WebsocketconnectionService ){}
  
  ngOnInit(): void {
    this.websocketService.GetConnection().subscribe({
      next:(response)=>{
        
        this.message=response.message;
      },
      error:(error)=>{
        console.log(error);
        this.message=error;
      }
    })
  }

}
