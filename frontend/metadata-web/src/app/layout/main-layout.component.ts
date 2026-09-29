import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { NzLayoutModule } from 'ng-zorro-antd/layout';
import { NzMenuModule } from 'ng-zorro-antd/menu';
import { NzIconModule } from 'ng-zorro-antd/icon';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    NzLayoutModule,
    NzMenuModule,
    NzIconModule
  ],
  template: `
    <nz-layout class="app-layout">
      <nz-sider nzCollapsible [(nzCollapsed)]="collapsed" [nzTrigger]="null">
        <div class="logo">
          <span nz-icon nzType="database"></span>
          @if (!collapsed) {
            <span class="logo-text">元数据查询平台</span>
          }
        </div>
        <ul nz-menu nzTheme="dark" nzMode="inline">
          <li nz-menu-item routerLink="/connections" routerLinkActive="ant-menu-item-selected">
            <span nz-icon nzType="api"></span>
            <span>数据库连接</span>
          </li>
          <li nz-menu-item routerLink="/metadata" routerLinkActive="ant-menu-item-selected">
            <span nz-icon nzType="table"></span>
            <span>元数据管理</span>
          </li>
          <li nz-menu-item routerLink="/data-query" routerLinkActive="ant-menu-item-selected">
            <span nz-icon nzType="search"></span>
            <span>数据查询</span>
          </li>
        </ul>
      </nz-sider>
      <nz-layout>
        <nz-header class="app-header">
          <span class="trigger" nz-icon [nzType]="collapsed ? 'menu-unfold' : 'menu-fold'"
                (click)="collapsed = !collapsed"></span>
        </nz-header>
        <nz-content class="app-content">
          <router-outlet />
        </nz-content>
      </nz-layout>
    </nz-layout>
  `,
  styles: [`
    .app-layout { min-height: 100vh; }
    .logo {
      height: 48px; margin: 16px; display: flex; align-items: center; gap: 10px;
      color: #fff; font-size: 16px; font-weight: 600; overflow: hidden; white-space: nowrap;
    }
    .logo [nz-icon] { font-size: 24px; }
    .app-header {
      background: #fff; padding: 0 20px; display: flex; align-items: center;
      box-shadow: 0 1px 4px rgba(0,21,41,.08);
    }
    .trigger { font-size: 18px; cursor: pointer; }
    .app-content { margin: 16px; }
  `]
})
export class MainLayoutComponent {
  collapsed = false;
}
