# Near Market

A standalone Angular storefront for the supplied general merchandise catalog. It includes a responsive product discovery homepage and checkout flow, with a shared signal-based cart.

## Run locally

```bash
npm install
npm start
```

Open `http://localhost:4200`. The app uses Angular standalone components and Angular Router. No Tailwind build step is required; component styles use responsive CSS, and icons use `@lucide/angular`.

The development proxy forwards `/api/*` to the backend at `http://localhost:5124`. Start the .NET API with its `http` launch profile and restart `ng serve` after changing proxy settings. Public catalog and category endpoints load without a login; cart and order endpoints require an account and bearer token.

## Structure

```text
src/
  app/
    core/cart.service.ts          # Shared cart signals and mutations
    core/auth.service.ts          # Login and access-token state
    core/catalog.service.ts       # Product and category API
    pages/home/                    # Discovery homepage
    pages/checkout/                # Address, payment, and order review
    app.routes.ts
    app.component.ts
  main.ts
  styles.css
```

The supplied API seeds a general merchandise catalog (fitness and sports, personal care, auto accessories, electronics, apparel, toys, and related categories), not grocery inventory. The product/category catalog, authenticated cart, sign-in, and order checkout are connected to the supplied API. That API does not currently expose nearby stores, saved addresses, payment processing/method selection, delivery estimates, or delivery/tax fees; the screens call out those limits instead of displaying fabricated values. Guest cart items are synced to the account after sign-in. The current catalog seeder assigns the category image to every product in that category, so product cards can share an image until product-specific images are added.
