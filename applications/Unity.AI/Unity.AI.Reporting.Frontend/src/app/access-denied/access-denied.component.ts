import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-access-denied',
  standalone: true,
  imports: [],
  templateUrl: './access-denied.component.html',
  styleUrls: ['./access-denied.component.css']
})
export class AccessDeniedComponent implements OnInit {

  ngOnInit(): void {
    console.log('=== ACCESS DENIED COMPONENT LOADED ===');
    console.log('No valid authentication token found');
  }
}
