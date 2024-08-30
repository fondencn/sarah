import { Component, OnInit } from '@angular/core';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrl: './home.component.css'
})
export class HomeComponent implements OnInit {
  message: string = "Init";



  constructor(private authService: AuthService) {

  }


  ngOnInit() {
    var isLoggedIn = this.authService.isLoggedIn();

    if(isLoggedIn) 
    {
      this.message = "Someone is loggedin";
    } else {
      this.message = "No user is loggedin";
    }
  }
}
