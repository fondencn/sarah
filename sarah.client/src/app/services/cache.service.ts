import { Injectable } from '@angular/core';
import { BaseDataDto, BaseDataService } from './api-client';
import { catchError, Observable, of, tap } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
/**
 * Service to manage caching of data.
 */
export class CacheService {

    public static readonly DEVICE_TYPES_KEY = 'baseData.DeviceTypes';
    public static readonly NETWORK_ELEMENTS_KEY = 'baseData.NetworkElements';
    public static readonly ROOMS_KEY = 'baseData.Rooms';
    public static readonly MOBILEPHONES_KEY = 'baseData.MobilePhones';
    public static readonly TRACKERS_KEY = 'baseData.Trackers';


    /**
     * Internal cache storage.
     */
    private cache: Map<string, any> = new Map();

    /**
     * Creates an instance of CacheService.
     * @param baseDataService - The service to fetch base data from the API.
     */
    constructor(private baseDataService: BaseDataService) {}

    /**
     * Sets a value in the cache.
     * @param key - The key to identify the cached value.
     * @param value - The value to cache.
     */
    private set(key: string, value: any): void {
        this.cache.set(key, value);
    }

    /**
     * Retrieves a value from the cache.
     * @param key - The key to identify the cached value.
     * @returns The cached value if present, otherwise null.
     */
    public get<T>(key: string): T | null {
        return this.cache.has(key) ? (this.cache.get(key) as T) : null;
    }

    /**
     * Clears all values from the cache.
     */
    public clear(): void {
        this.cache.clear();
    }

    /**
     * Updates the cache with data from the API.
     * @returns An observable of the base data or null.
     */
    public update(): Observable<BaseDataDto | null> {
        this.clear();
        return this.baseDataService.apiBaseDataGet().pipe(
            tap((response: BaseDataDto) => {
            this.set(CacheService.DEVICE_TYPES_KEY, response.deviceTypeEnumeration);
            this.set(CacheService.NETWORK_ELEMENTS_KEY, response.networkElements);
            this.set(CacheService.ROOMS_KEY, response.rooms);
            this.set(CacheService.TRACKERS_KEY, response.trackers);
            this.set(CacheService.MOBILEPHONES_KEY, response.mobilePhones);
            })
        );
    }
}
 