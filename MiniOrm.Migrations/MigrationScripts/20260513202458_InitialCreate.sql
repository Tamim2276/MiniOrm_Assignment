-- up
CREATE TABLE IF NOT EXISTS orders (
  id SERIAL PRIMARY KEY,
  order_date TIMESTAMP NOT NULL,
  customer_name TEXT NOT NULL,
  total_amount NUMERIC NOT NULL
);

CREATE TABLE IF NOT EXISTS products (
  id SERIAL PRIMARY KEY,
  name TEXT NOT NULL,
  price NUMERIC NOT NULL,
  discount NUMERIC NULL,
  in_stock BOOLEAN NOT NULL
);
-- down
DROP TABLE IF EXISTS orders;
DROP TABLE IF EXISTS products;
