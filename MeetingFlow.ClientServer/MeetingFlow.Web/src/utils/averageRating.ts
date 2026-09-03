export function formatAverageRating(ratings: number[] | undefined | null): string {
  if (!ratings?.length) {
    return "N/A";
  }

  const sum = ratings.reduce((total, rating) => total + rating, 0);
  return (sum / ratings.length).toFixed(1);
}
