using System;
using System.Linq;
using System.Threading;
using Avancira.Application.LessonMaterials.Dtos;
using Avancira.Application.Lessons.Specifications;
using Avancira.Application.Persistence;
using Avancira.Domain.Common.Exceptions;
using Avancira.Domain.Lessons;
using Mapster;

namespace Avancira.Application.LessonMaterials;

public class LessonMaterialService : ILessonMaterialService
{
    private readonly IRepository<LessonMaterial> _lessonMaterialRepository;
    private readonly IRepository<Lesson> _lessonRepository;

    public LessonMaterialService(
        IRepository<LessonMaterial> lessonMaterialRepository,
        IRepository<Lesson> lessonRepository)
    {
        _lessonMaterialRepository = lessonMaterialRepository;
        _lessonRepository = lessonRepository;
    }

    public async Task<IReadOnlyCollection<LessonMaterialDto>> GetByLessonIdAsync(int lessonId, CancellationToken cancellationToken = default)
    {
        var materials = await _lessonMaterialRepository.ListAsync(m => m.LessonId == lessonId, cancellationToken);
        return materials
            .OrderBy(material => material.UploadedAtUtc)
            .Adapt<IReadOnlyCollection<LessonMaterialDto>>();
    }

    public async Task<LessonMaterialDto> CreateAsync(LessonMaterialCreateDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var lesson = await _lessonRepository.FirstOrDefaultAsync(new LessonByIdSpec(request.LessonId), cancellationToken)
            ?? throw new AvanciraNotFoundException($"Lesson '{request.LessonId}' not found.");

        var material = LessonMaterial.Create(
            request.LessonId,
            request.UploadedByUserId,
            request.FileName,
            request.FileType,
            request.FileSizeBytes,
            request.FileUrl,
            request.MaterialType,
            request.Description,
            request.IsSharedWithStudent);

        await _lessonMaterialRepository.AddAsync(material, cancellationToken);

        return material.Adapt<LessonMaterialDto>();
    }

    public async Task<LessonMaterialDto> UpdateScanStatusAsync(LessonMaterialScanUpdateDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var material = await _lessonMaterialRepository.GetByIdAsync(request.MaterialId, cancellationToken)
            ?? throw new AvanciraNotFoundException($"Lesson material '{request.MaterialId}' not found.");

        material.UpdateScanStatus(request.ScanStatus, request.ScanResult);
        await _lessonMaterialRepository.UpdateAsync(material, cancellationToken);

        return material.Adapt<LessonMaterialDto>();
    }
}
