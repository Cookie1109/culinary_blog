"""Verify the documented local stack serves the application and seeded recipe images."""

import json
import sys
from urllib.parse import urlsplit
from urllib.request import urlopen


def check(base_url):
    base_url = base_url.rstrip("/")
    if urlsplit(base_url).hostname not in {"localhost", "127.0.0.1"}:
        raise ValueError("This startup check is for a local Docker stack.")

    def read(path):
        with urlopen(base_url + path, timeout=30) as response:
            if response.status != 200:
                raise RuntimeError(f"Startup request failed: {path}")
            return response.read()

    read("/health/ready")
    page = read("/")
    if b"Culinary Blog" not in page:
        raise RuntimeError("The application home page was not served.")
    read("/login")
    read("/register")
    recipes = json.loads(read("/api/v1/recipes?pageSize=12"))["data"]
    if not recipes:
        raise RuntimeError("The fresh database has no sample recipes.")
    recipe = next((item for item in recipes if item.get("primaryImageUrl")), None)
    if recipe is None:
        raise RuntimeError("The sample recipes have no images.")
    read("/recipes/" + recipe["slug"])
    image_path = urlsplit(recipe["primaryImageUrl"]).path
    image = read(image_path)
    if not image:
        raise RuntimeError("The sample recipe image is empty.")
    print("Fresh local startup passed: home, authentication pages, database, recipes and stored image.")


if __name__ == "__main__":
    if len(sys.argv) != 2:
        raise SystemExit("Usage: python scripts/check-local-startup.py http://localhost:8080")
    check(sys.argv[1])
