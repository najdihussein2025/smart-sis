"""LLM tools. Each tool = one .NET endpoint call, returning compact JSON. Built in phase 7."""

from app.clients.sis_api import SisApiClient

# TODO(phase-7): point these at the real endpoints once phases 3-5 exist.


def get_my_profile(api: SisApiClient) -> dict:
    return api.get("/api/profile")


TOOLS = {
    "get_my_profile": get_my_profile,
    # "get_my_grades": ...,     phase 5
    # "get_my_schedule": ...,   phase 3
    # "get_deadlines": ...,     phase 3
}
