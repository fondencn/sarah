import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { RoomsComponent } from './rooms.component';
import { RoomsClient } from '../services/api/room-service/api/rooms.service';
import { DashboardRuntimeService } from '../services/dashboard-runtime.service';
import { DialogService } from '../services/dialog.service';
import { LoggingService } from '../services/logging.service';
import { EventEmitter } from '@angular/core';
import { DashboardItemTypeDto } from '../models/api-types';

describe('RoomsComponent', () => {
  let component: RoomsComponent;
  let roomsServiceSpy: jasmine.SpyObj<RoomsClient>;
  let dashboardServiceSpy: jasmine.SpyObj<DashboardRuntimeService>;
  let dialogServiceSpy: jasmine.SpyObj<DialogService>;
  let loggerSpy: jasmine.SpyObj<LoggingService>;

  const mockRooms = [
    { id: 1, name: 'Kitchen' },
    { id: 2, name: 'Living Room' },
    { id: 3, name: 'Bedroom' }
  ] as any[];

  const mockDashboardItems = [
    { itemId: 2, itemType: DashboardItemTypeDto.NUMBER_2, title: 'Living Room' },
    { itemId: 10, itemType: DashboardItemTypeDto.NUMBER_0, title: 'Some Device' }
  ] as any[];

  beforeEach(() => {
    roomsServiceSpy = jasmine.createSpyObj('RoomsClient', [
      'apiRoomsGet', 'apiRoomsIdDelete', 'apiRoomsPost', 'apiRoomsIdPut', 'apiRoomsIdFavouriteIsFavouritePut'
    ]);
    dashboardServiceSpy = jasmine.createSpyObj('DashboardRuntimeService', [
      'apiDashboardGet', 'apiDashboardPost', 'apiDashboardItemIdItemTypeDelete'
    ]);
    dialogServiceSpy = jasmine.createSpyObj('DialogService', ['showDialog', 'showConfirmDialog']);
    (dialogServiceSpy as any).dialogClosed = new EventEmitter();
    loggerSpy = jasmine.createSpyObj('LoggingService', ['debug', 'info', 'warn', 'error']);
    roomsServiceSpy.apiRoomsIdPut.and.callFake(((id: number, room: any) => of(room)) as any);

    roomsServiceSpy.apiRoomsGet.and.returnValue(of(mockRooms.slice()) as any);
    dashboardServiceSpy.apiDashboardGet.and.returnValue(of(mockDashboardItems));

    TestBed.configureTestingModule({
      providers: [
        RoomsComponent,
        { provide: RoomsClient, useValue: roomsServiceSpy },
        { provide: DashboardRuntimeService, useValue: dashboardServiceSpy },
        { provide: DialogService, useValue: dialogServiceSpy },
        { provide: LoggingService, useValue: loggerSpy }
      ]
    });

    component = TestBed.inject(RoomsComponent);
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('retrieveRooms()', () => {
    it('populates pinnedRoomIds with room ids present in dashboard', () => {
      component.retrieveRooms();
      expect(component.pinnedRoomIds.has(2)).toBeTrue();
    });

    it('does not include rooms not on the dashboard in pinnedRoomIds', () => {
      component.retrieveRooms();
      expect(component.pinnedRoomIds.has(1)).toBeFalse();
      expect(component.pinnedRoomIds.has(3)).toBeFalse();
    });

    it('ignores dashboard items of other types (devices, persons, etc.)', () => {
      component.retrieveRooms();
      // itemId=10 is a Device (type NUMBER_0), must not be in pinnedRoomIds
      expect(component.pinnedRoomIds.has(10)).toBeFalse();
    });

    it('sets isLoading to false after loading', () => {
      component.retrieveRooms();
      expect(component.isLoading).toBeFalse();
    });
  });

  describe("onRoomDrop()", () => {
    it("assigns sortOrder by position and saves the rooms that changed", () => {
      component.rooms = mockRooms.map((room, index) => ({ ...room, sortOrder: index }));

      component.onRoomDrop({ previousIndex: 0, currentIndex: 2 } as any);

      expect(component.rooms.map(room => [room.id, room.sortOrder])).toEqual([[2, 0], [3, 1], [1, 2]]);
      expect(roomsServiceSpy.apiRoomsIdPut).toHaveBeenCalledTimes(3);
      expect(roomsServiceSpy.apiRoomsIdPut).toHaveBeenCalledWith(1, jasmine.objectContaining({ sortOrder: 2 }));
      expect(component.isSavingOrder).toBeFalse();
    });

    it("only saves rooms whose sortOrder changed", () => {
      component.rooms = mockRooms.map((room, index) => ({ ...room, sortOrder: index }));

      component.onRoomDrop({ previousIndex: 0, currentIndex: 1 } as any);

      expect(roomsServiceSpy.apiRoomsIdPut).toHaveBeenCalledTimes(2);
      expect(roomsServiceSpy.apiRoomsIdPut).not.toHaveBeenCalledWith(3, jasmine.anything());
    });

    it("does not save when fewer than two rooms are available", () => {
      component.rooms = [mockRooms[0]];

      component.onRoomDrop({ previousIndex: 0, currentIndex: 0 } as any);

      expect(roomsServiceSpy.apiRoomsIdPut).not.toHaveBeenCalled();
    });

    it("restores the previous order and displays an error when saving fails", () => {
      component.rooms = mockRooms.map((room, index) => ({ ...room, sortOrder: index }));
      roomsServiceSpy.apiRoomsIdPut.and.returnValue(throwError(() => new Error("request failed")));

      component.onRoomDrop({ previousIndex: 0, currentIndex: 2 } as any);

      expect(component.rooms.map(room => room.id)).toEqual([1, 2, 3]);
      expect(component.orderSaveError).toContain("previous order has been restored");
      expect(loggerSpy.error).toHaveBeenCalled();
      expect(component.isSavingOrder).toBeFalse();
    });
  });

  describe('pinToDashboard()', () => {
    it('calls delete when the room is already pinned', () => {
      component.pinnedRoomIds.add(1);
      dashboardServiceSpy.apiDashboardItemIdItemTypeDelete.and.returnValue(of(undefined as any));

      component.pinToDashboard(mockRooms[0]);

      expect(dashboardServiceSpy.apiDashboardItemIdItemTypeDelete).toHaveBeenCalledWith(1, DashboardItemTypeDto.NUMBER_2);
      expect(component.pinnedRoomIds.has(1)).toBeFalse();
    });

    it('calls post when the room is not yet pinned', () => {
      dashboardServiceSpy.apiDashboardPost.and.returnValue(of({} as any));

      component.pinToDashboard(mockRooms[0]);

      expect(dashboardServiceSpy.apiDashboardPost).toHaveBeenCalled();
      expect(component.pinnedRoomIds.has(1)).toBeTrue();
    });

    it('does not call post when room is already pinned', () => {
      component.pinnedRoomIds.add(1);
      dashboardServiceSpy.apiDashboardItemIdItemTypeDelete.and.returnValue(of(undefined as any));

      component.pinToDashboard(mockRooms[0]);

      expect(dashboardServiceSpy.apiDashboardPost).not.toHaveBeenCalled();
    });

    it('builds the room detail route for the Details action', () => {
      expect(component.roomDetailsLink(mockRooms[0])).toEqual(['/rooms', 1]);
    });
  });
});
