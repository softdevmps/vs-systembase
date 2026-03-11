from __future__ import annotations

from pydantic import BaseModel, Field
from typing import Optional


class InferRequest(BaseModel):
    projectId: int = Field(..., ge=1)
    input: str = Field(..., min_length=1)
    contextJson: Optional[str] = None


class RunExecuteRequest(BaseModel):
    projectId: int = Field(..., ge=1)
    runType: str = Field(..., min_length=1)
    inputJson: Optional[str] = None


class ExportDockerRequest(BaseModel):
    serviceName: Optional[str] = None
    imageTag: Optional[str] = None
    hostPort: int = Field(default=8010, ge=1, le=65535)
    containerPort: int = Field(default=8010, ge=1, le=65535)
    extraEnv: dict[str, str] = Field(default_factory=dict)


class DatasetGenerateRequest(BaseModel):
    topics: list[str] = Field(default_factory=list)
    datasetName: Optional[str] = None
    maxWikipediaResults: Optional[int] = Field(default=None, ge=1, le=50)
    maxExpandedQueries: Optional[int] = Field(default=None, ge=1, le=200)
    chunkSize: Optional[int] = Field(default=None, ge=200, le=8000)
    chunkOverlap: Optional[int] = Field(default=None, ge=0, le=3000)
    sleepSeconds: Optional[float] = Field(default=None, ge=0, le=10)
    resetTopicFolders: bool = True


class DatasetMergeRequest(BaseModel):
    sourcePaths: list[str] = Field(default_factory=list)
    datasetName: Optional[str] = None
    deduplicate: bool = True
