import { Injectable } from '@angular/core';
import { Observable, Subscription } from 'rxjs';
import { webSocket } from "rxjs/webSocket";

@Injectable({
  providedIn: 'root'
})
export class WebsocketconnectionService {

  private _websocketConnection = webSocket('ws://localhost:4452');

  constructor() { }

  GetConnection():Observable<any>{
    return this._websocketConnection;
  }

  CloseConnection():void{
    this._websocketConnection.complete();
  }

  SendMessage(message:string){
    //this._websocketConnection.next(JSON.stringify({message:message}));
  }
}
