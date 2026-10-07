export interface ReferenceDataItem {
  id: string;
  name: string;
  description: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export type JobCategory = ReferenceDataItem;
export type Skill = ReferenceDataItem;

export interface Country {
  id: string;
  name: string;
  code: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export interface Province {
  id: string;
  countryId: string;
  countryName: string;
  name: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export interface City {
  id: string;
  provinceId: string;
  provinceName: string;
  countryId: string;
  countryName: string;
  name: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export interface Location {
  id: string;
  name: string;
  type: 'Country' | 'Province' | 'City';
  parentId: string | null;
  parentName: string | null;
  countryId: string | null;
  countryName: string | null;
  isActive: boolean;
}
