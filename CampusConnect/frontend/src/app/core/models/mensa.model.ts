export interface MensaDish {
  name: string;
  nameLines?: string[];
  category: string;
  priceStudent: number;
  allergens: string | null;
  isVegetarian: boolean;
  isVegan: boolean;
}

export interface MensaDay {
  date: string;
  dishes: MensaDish[];
}
