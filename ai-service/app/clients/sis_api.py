"""Calls the .NET API with the student's own JWT, so the AI can only see that student's data."""

from typing import Any

import httpx

from app.config import get_settings


class SisApiClient:
    def __init__(self, student_token: str, timeout: float = 10.0) -> None:
        self._client = httpx.Client(
            base_url=get_settings().sis_api_base_url,
            headers={"Authorization": f"Bearer {student_token}"},
            timeout=timeout,
        )

    def get(self, path: str, **params: Any) -> Any:
        response = self._client.get(path, params=params or None)
        response.raise_for_status()
        return response.json()

    def close(self) -> None:
        self._client.close()

    def __enter__(self) -> "SisApiClient":
        return self

    def __exit__(self, *_: object) -> None:
        self.close()
