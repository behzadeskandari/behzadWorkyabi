import { Component, OnInit, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../auth/services/auth.service';
import { NotificationsService } from '../../notifications/notifications.service';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss'
})
export class HeaderComponent implements OnInit {
  readonly authService = inject(AuthService);
  readonly notifications = inject(NotificationsService);

  ngOnInit(): void {
    if (this.authService.isAuthenticated()) {
      this.notifications.refreshUnreadCount().subscribe({ error: () => undefined });
    }
  }

  logout(): void {
    this.authService.logout().subscribe();
  }
}
