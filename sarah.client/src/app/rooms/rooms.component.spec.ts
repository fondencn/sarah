import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
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

    roomsServiceSpy.apiRoomsGet.and.returnValue(of(mockRooms) as any);
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
  });
});
