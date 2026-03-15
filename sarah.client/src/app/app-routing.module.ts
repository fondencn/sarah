import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { HomeComponent } from './home/home.component';
import { DevicesComponent } from './devices/devices.component';
import { PersonsComponent } from './persons/persons.component';
import { AdminComponent } from './admin/admin.component';
import { RoomsComponent } from './rooms/rooms.component';
import { HomedetailsComponent } from './home/details/homedetails.component';
import { PersondetailsComponent } from './home/details/person/persondetails/persondetails.component';
import { DevicedetailsComponent } from './home/details/device/devicedetails/devicedetails.component';
import { RoomdetailsComponent } from './home/details/room/roomdetails/roomdetails.component';
import { AlarmsComponent } from './alarms/alarms.component';

const routes: Routes = [
  { path: "home", component: HomeComponent },
  { path: "home/details", component: HomedetailsComponent },
  { path: "home/details/person", component: PersondetailsComponent },
  { path: "home/details/device", component: DevicedetailsComponent },
  { path: "home/details/room", component: RoomdetailsComponent },
  { path: "admin", component: AdminComponent },
  { path: "devices", component: DevicesComponent },
  { path: "persons", component: PersonsComponent },
  { path: "rooms", component: RoomsComponent },
  { path: "alarms", component: AlarmsComponent },
  { path: '', redirectTo: '/home', pathMatch: 'full' }, // redirect to `home`
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
