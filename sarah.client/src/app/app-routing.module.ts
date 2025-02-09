import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { HomeComponent } from './home/home.component';
import { DevicesComponent } from './devices/devices.component';
import { PersonsComponent } from './persons/persons.component';
import { AdminComponent } from './admin/admin.component';

const routes: Routes = [
  { path: "home",component: HomeComponent},
  { path: "admin",component: AdminComponent},
  { path: "devices",component: DevicesComponent},
  { path: "persons",component: PersonsComponent},
  { path: '',   redirectTo: '/home', pathMatch: 'full' }, // redirect to `home`
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
