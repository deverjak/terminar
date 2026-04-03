import { useState, useMemo } from 'react';
import type { CourseListItem, CourseFilters, DisplayStatus, TemporalBucket, SortField, CourseType } from '../types';

function getDisplayStatus(course: CourseListItem, now: Date): string {
  if (course.status === 'Active' && course.lastSessionEndsAt && new Date(course.lastSessionEndsAt) <= now) {
    return 'Ended';
  }
  return course.status;
}

const DEFAULT_FILTERS: CourseFilters = {
  temporalBucket: 'all',
  search: '',
  statuses: [],
  courseType: null,
  tags: [],
  sortField: 'firstSessionAt',
  sortDirection: 'asc',
};

function classifyBucket(course: CourseListItem, now: Date): TemporalBucket {
  if (course.status === 'Cancelled' || course.status === 'Completed') return 'past';
  if (course.lastSessionEndsAt && new Date(course.lastSessionEndsAt) <= now) return 'past';
  if (!course.firstSessionAt || new Date(course.firstSessionAt) > now) return 'upcoming';
  return 'ongoing';
}

export function useCoursesFilter(courses: CourseListItem[]) {
  const [filters, setFilters] = useState<CourseFilters>(DEFAULT_FILTERS);

  const availableTags = useMemo(() => {
    const tagSet = new Set<string>();
    courses.forEach(c => c.tags.forEach(t => tagSet.add(t)));
    return [...tagSet].sort();
  }, [courses]);

  const hasActiveFilters = useMemo(() => {
    return (
      filters.temporalBucket !== 'all' ||
      filters.search.trim() !== '' ||
      filters.statuses.length > 0 ||
      filters.courseType !== null ||
      filters.tags.length > 0
    );
  }, [filters]);

  const filteredCourses = useMemo(() => {
    const now = new Date();
    let result = courses;

    // Temporal filter
    if (filters.temporalBucket !== 'all') {
      result = result.filter(c => classifyBucket(c, now) === filters.temporalBucket);
    }

    // Search filter
    if (filters.search.trim()) {
      const q = filters.search.trim().toLowerCase();
      result = result.filter(c => c.title.toLowerCase().includes(q));
    }

    // Status filter (matches against display status, so 'Ended' works for Active+past courses)
    if (filters.statuses.length > 0) {
      result = result.filter(c => (filters.statuses as string[]).includes(getDisplayStatus(c, now)));
    }

    // Course type filter
    if (filters.courseType !== null) {
      result = result.filter(c => c.courseType === filters.courseType);
    }

    // Tag filter
    if (filters.tags.length > 0) {
      result = result.filter(c => filters.tags.some(t => c.tags.includes(t)));
    }

    // Sort
    result = [...result].sort((a, b) => {
      let cmp = 0;
      if (filters.sortField === 'title') {
        cmp = a.title.localeCompare(b.title);
      } else if (filters.sortField === 'capacity') {
        cmp = a.capacity - b.capacity;
      } else {
        // firstSessionAt — nulls last for asc, nulls first for desc
        if (!a.firstSessionAt && !b.firstSessionAt) cmp = 0;
        else if (!a.firstSessionAt) cmp = 1;
        else if (!b.firstSessionAt) cmp = -1;
        else cmp = new Date(a.firstSessionAt).getTime() - new Date(b.firstSessionAt).getTime();
      }
      return filters.sortDirection === 'asc' ? cmp : -cmp;
    });

    return result;
  }, [courses, filters]);

  function setTemporalBucket(temporalBucket: TemporalBucket) {
    setFilters(f => ({ ...f, temporalBucket }));
  }

  function setSearch(search: string) {
    setFilters(f => ({ ...f, search }));
  }

  function setStatuses(statuses: DisplayStatus[]) {
    setFilters(f => ({ ...f, statuses }));
  }

  function setCourseType(courseType: CourseType | null) {
    setFilters(f => ({ ...f, courseType }));
  }

  function setTagsFilter(tags: string[]) {
    setFilters(f => ({ ...f, tags }));
  }

  function toggleSort(field: SortField) {
    setFilters(f => ({
      ...f,
      sortField: field,
      sortDirection: f.sortField === field && f.sortDirection === 'asc' ? 'desc' : 'asc',
    }));
  }

  function clearAll() {
    setFilters(DEFAULT_FILTERS);
  }

  return {
    filters,
    filteredCourses,
    availableTags,
    hasActiveFilters,
    setTemporalBucket,
    setSearch,
    setStatuses,
    setCourseType,
    setTagsFilter,
    toggleSort,
    clearAll,
  };
}
