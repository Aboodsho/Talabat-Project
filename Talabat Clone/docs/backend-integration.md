# Backend analysis and UI contract

Backend reviewed: `E-Commerce` ASP.NET Core API (`API` project), using the supplied OpenAPI document and controller, DTO, handler, and catalog seeder source.

## What the backend represents

This is a general merchandise store, not a grocery or hyper-local delivery backend. The development catalog seeds 10 categories and 100 products across health and beauty, supplements, automotive, electronics, sports, clothing, toys, and exercise equipment. Some category names and descriptions are Arabic. Each seeded product currently receives its category's image, so products within one category may share a picture.

The product DTO includes `id`, `name`, `description`, `price`, `categoryName`, `images`, and `isAvailable`. It does not include product weight, sale price, discount, rating, per-product store, or delivery time. Prices are decimals without a currency code.

## Customer API used by the app

| API | Contract | UI behavior |
| --- | --- | --- |
| `GET /api/categories` | Paged categories (`PageNumber`, `PageSize`) | Renders category tiles and filters the catalog by category ID. |
| `GET /api/products` | Paged products; supports category, search, minimum/maximum price | Loads the catalog, applies search/category filters, and uses the API page count for “Load more”. |
| `POST /api/auth/login` | Email/password; returns `accessToken` and user data | Signs in and stores the bearer token for authenticated requests. |
| `POST /api/auth/register` | Full name, email, password; sends an email-confirmation link | Offers a create-account state from the homepage account panel. In development the backend logs the confirmation link to its console. |
| `POST /api/auth/confirm-email` | User ID and confirmation token | The email link opens the Angular confirmation route, which verifies the token with the API. |
| `GET /api/cart` | Authenticated cart with product IDs, unit prices, quantities, line totals, and stock availability | Loads the customer's server cart and disables checkout if an item is unavailable. |
| `POST /api/cart/items` | `{ productId, quantity }` | Adds items and syncs a guest bag after sign-in. |
| `PATCH /api/cart/items/{productId}/decrease` | `{ amount }` | Decreases an item quantity. |
| `DELETE /api/cart/items/{productId}` | Authenticated | Removes an item. |
| `POST /api/orders/checkout` | `{ street, city, state, postalCode, country }`; authenticated | Submits the address; the backend validates stock, creates the order, decreases inventory, and clears the cart. Returns an order ID. |

The development API launch profile is `http://localhost:5124` (Swagger at `/swagger/index.html`). The Angular dev server proxies `/api` there through `proxy.conf.json`.

## Backend capabilities the current UI does not claim

The API has no nearby-store or location search, saved-address CRUD, discount or sale pricing, payment-method/payment-processing fields, delivery ETA, or delivery/tax-fee calculation. The checkout page asks for a shipping address and shows payment preference options for the design, but explicitly says the backend does not save/process that selection. The order total is therefore the cart item subtotal returned by the available cart data.

The API also has customer order history, cancellation, wishlists, reviews, and admin endpoints. Those are not part of the current homepage and checkout scope.
