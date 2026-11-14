"""Calls the .NET API with the student's own JWT, so the AI can only see that student's data."""

from typing import Any

import httpx


class SisApiError(Exception):
    """The .NET API answered with an error or couldn't be reached (`status` is None then)."""

    def __init__(self, status: int | None, message: str) -> None:
        super().__init__(message)
        self.status = status


class SisApiClient:
    """One client per chat request. The token only ever goes in the Authorization header; it is never logged."""

    def __init__(
        self,
        student_token: str,
        base_url: str,
        timeout: float = 10.0,
        transport: httpx.BaseTransport | None = None,
    ) -> None:
        self._client = httpx.Client(
            base_url=base_url,
            headers={"Authorization": f"Bearer {student_token}"},
            timeout=timeout,
            transport=transport,
        )

    def get(self, path: str, **params: Any) -> Any:
        try:
            response = self._client.get(path, params=params or None)
        except httpx.HTTPError as ex:
            raise SisApiError(None, f"SIS API unreachable ({type(ex).__name__})") from ex
        if response.status_code >= 400:
            raise SisApiError(response.status_code, _problem_title(response))
        return response.json()

    def close(self) -> None:
        self._client.close()

    def __enter__(self) -> "SisApiClient":
        return self

    def __exit__(self, *_: object) -> None:
        self.close()


def _problem_title(response: httpx.Response) -> str:
    try:
        body = response.json()
        if isinstance(body, dict) and isinstance(body.get("title"), str):
            return body["title"]
    except ValueError:
        pass
    return f"HTTP {response.status_code}"
