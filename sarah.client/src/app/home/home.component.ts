import { Component, OnInit } from '@angular/core';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrl: './home.component.css'
})
export class HomeComponent implements OnInit {

  username: string = '';
  password: string = '';

  btnLogin_Click(e: Event) : void {
    // Implement your login logic here
    console.log('Username:', this.username);
    console.log('Password:', this.password);
    // Add authentication logic and navigate to the next page upon successful login
    this.authService.login(this.username, this.password);
  }


  constructor(private authService: AuthService) {

  }


  ngOnInit() {
    var isLoggedIn = this.authService.isLoggedIn();

  }
}
