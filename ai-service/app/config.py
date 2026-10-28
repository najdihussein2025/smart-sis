from functools import lru_cache

from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    """Read from environment / .env. Never hardcode keys."""

    model_config = SettingsConfigDict(env_file=".env", extra="ignore")

    service_key: str = ""
    sis_api_base_url: str = "http://localhost:5080"
    llm_api_key: str = ""
    llm_model: str = ""
    sql_connection_string: str = ""


@lru_cache
def get_settings() -> Settings:
    return Settings()
