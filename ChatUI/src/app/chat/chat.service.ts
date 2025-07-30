import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { map } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class ChatService {
  constructor(private http: HttpClient) { }

  getBotReply(prompt: string): Observable<string> {    
    return this.http.post<any>('https://localhost:7143/api/Chat/send', { message: prompt }).pipe(
      map(res => res.reply || 'No response')
    );
  }
}
