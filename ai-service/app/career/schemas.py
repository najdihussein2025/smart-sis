"""Request/response models for POST /career/explain. The .NET API sends camelCase JSON."""

from typing import Literal

from pydantic import BaseModel, ConfigDict, Field
from pydantic.alias_generators import to_camel


class _CamelModel(BaseModel):
    model_config = ConfigDict(alias_generator=to_camel, populate_by_name=True)


class ExplainCareer(_CamelModel):
    """A recommended career and its scores. No personal data."""

    name: str = Field(min_length=1, max_length=100)
    match_pct: int = Field(ge=0, le=100)
    quiz_pct: int = Field(ge=0, le=100)
    grade_pct: int | None = Field(default=None, ge=0, le=100)
    top_traits: list[str] = Field(default_factory=list, max_length=10)


class ExplainTrait(_CamelModel):
    name: str = Field(min_length=1, max_length=60)
    score: int = Field(ge=0, le=100)


class CareerExplainRequest(_CamelModel):
    """Sent by the .NET API. Holds no name, id or email (minimal PII)."""

    careers: list[ExplainCareer] = Field(min_length=1, max_length=3)
    traits: list[ExplainTrait] = Field(default_factory=list, max_length=30)
    completed_courses: list[str] = Field(default_factory=list, max_length=100)
    uses_grades: bool = False


class CareerExplainResponse(_CamelModel):
    explanation: str
    source: Literal["llm", "fallback"]
