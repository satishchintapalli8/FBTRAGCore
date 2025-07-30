import { Component } from '@angular/core';
import { ChatService } from './chat.service';

@Component({
  selector: 'app-chat-window',
  templateUrl: './chat-window.component.html',
  styleUrls: ['./chat-window.component.css']
})
export class ChatWindowComponent {
  userInput: string = '';
  messages: { text: string, sender: 'user' | 'bot' }[] = [];

  constructor(private chatService: ChatService) {}

  sendMessage() {
    if (!this.userInput.trim()) return;

    const userMsg = { text: this.userInput, sender: 'user' } as { text: string, sender: 'user' | 'bot' };
    this.messages.push(userMsg);

    this.chatService.getBotReply(this.userInput).subscribe(botText => {
      this.messages.push({ text: botText, sender: 'bot' });
    });

    this.userInput = '';
  }
}
