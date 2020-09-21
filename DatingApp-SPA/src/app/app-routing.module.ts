import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { AuthGuard } from './_guards/auth.guard';
import { HomeComponent } from './home/home.component';
import { MemberListComponent } from './members/member-list/member-list.component';
import { MemberListResolver } from './_resolvers/member-list.resolver';
import { MemberDetailComponent } from './members/member-detail/member-detail.component';
import { MemberDetailResolver } from './_resolvers/member-detail.resolver';
import { MemberEditComponent } from './members/member-edit/member-edit.component';
import { MemberEditResolver } from './_resolvers/member-edit.resolver';
import { PreventUnsavedChanges } from './_guards/prevent-unsaved-changes.guard';
import { MessagesComponent } from './messages/messages.component';
import { MessagesResolver } from './_resolvers/messages.resolver';
import { ListsComponent } from './lists/lists.component';
import { ListsResolver } from './_resolvers/lists.resolver';
import { AdminPanelComponent } from './admin/admin-panel/admin-panel.component';
import { HelpComponent } from './help/help/help.component';

// loadChildren: () => import('./auth/auth.module').then(m => m.AuthModule)
const routes: Routes = [
  { path: 'home', component: HomeComponent },
  {
      path: '',
      runGuardsAndResolvers: 'always',
      canActivate: [AuthGuard],
      children: [
          { path: 'members', component: MemberListComponent,
               resolve: {users: MemberListResolver}},
          { path: 'members/:id', component: MemberDetailComponent,
              resolve: {user: MemberDetailResolver}},
          { path: 'member/edit', component: MemberEditComponent,
              resolve: {user: MemberEditResolver}, canDeactivate: [PreventUnsavedChanges]},
          { path: 'messages', component: MessagesComponent, resolve: {messages: MessagesResolver}},
          { path: 'lists', component: ListsComponent, resolve: {users: ListsResolver}},
          { path: 'admin', component: AdminPanelComponent, data: { roles: ['Admin', 'Moderator']} }
      ]
  },
  { path: 'help', component: HelpComponent },
  { path: '**', redirectTo: 'home', pathMatch: 'full'}
];

@NgModule({
    imports: [RouterModule.forRoot(routes)],
    exports: [RouterModule],
    providers: [AuthGuard]
})
export class AppRoutingModule {}
