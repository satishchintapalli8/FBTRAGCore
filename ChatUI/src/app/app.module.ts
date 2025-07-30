import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { ChatWindowComponent } from './chat/chat-window.component';
import { ChatMessageComponent } from './chat/chat-message.component';
import { NbChatModule, NbLayoutModule, NbThemeModule } from '@nebular/theme';
import { HttpClientModule } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';

@NgModule({
  declarations: [
    AppComponent,
    ChatWindowComponent,
    ChatMessageComponent
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    FormsModule,
    BrowserAnimationsModule,
    HttpClientModule,
    NbThemeModule.forRoot({ name: 'default' }),
    NbLayoutModule,
    NbChatModule,
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }
