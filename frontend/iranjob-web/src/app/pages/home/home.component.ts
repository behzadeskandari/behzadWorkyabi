import { Component } from '@angular/core';

@Component({
  selector: 'app-home',
  standalone: true,
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss'
})
export class HomeComponent {
  readonly title = 'ایران‌جاب';
  readonly welcomeMessage = 'به پلتفرم استخدام ایران‌جاب خوش آمدید.';
}
