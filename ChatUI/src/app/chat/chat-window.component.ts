import { Component } from '@angular/core';
import { ChatService } from './chat.service';

@Component({
  selector: 'app-chat-window',
  templateUrl: './chat-window.component.html',
  styleUrls: ['./chat-window.component.css']
})
export class ChatWindowComponent {
  userInput: string = '';
  messages: { text: string, sender: 'user' | 'bot', isProcessing: boolean }[] = [];
  loading = false;

  constructor(private chatService: ChatService) { }

  sendMessage() {
    if (!this.userInput.trim()) return;
    this.loading = true;
    const userMsg = { text: this.userInput, sender: 'user', isProcessing: false } as { text: string, sender: 'user' | 'bot', isProcessing: boolean };
    this.messages.push(userMsg);
    this.messages.push({ text: 'Processing...', sender: 'bot', isProcessing: true });
    this.chatService.getBotReply(this.userInput).subscribe(botText => {
      this.loading = false;
      let index = this.messages.findIndex(m => m.isProcessing);
      if (index !== -1) {
        this.messages.splice(index, 1);
      }
      let botTextToAdd: { text: string, sender: 'user' | 'bot', isProcessing: boolean } = { text: '', sender: 'bot', isProcessing: false };
      this.messages.push(botTextToAdd);
      const fullReply = botText;
      index = 0;
      const typingInterval = setInterval(() => {
        if (index < fullReply.length) {
          botTextToAdd.text += fullReply[index];
          index++;
        } else {
          clearInterval(typingInterval);
          botTextToAdd.isProcessing = false;
          this.loading = false;
        }
      }, 25);
    });

    this.userInput = '';
  }
}
