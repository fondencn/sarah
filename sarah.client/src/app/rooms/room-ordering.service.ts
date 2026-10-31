import { Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Observable } from "rxjs";
import { environment } from "../../environments/environment";

@Injectable({ providedIn: "root" })
export class RoomOrderingService {
  constructor(private http: HttpClient) { }

  saveOrder(roomIds: number[]): Observable<void> {
    return this.http.put<void>(`${environment.api.roomService}/api/Rooms/order`, roomIds);
  }
}
