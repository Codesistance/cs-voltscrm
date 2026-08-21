export function AnimatedBackground() {
  return (
    <div className="absolute inset-0 -z-10 overflow-hidden bg-[oklch(0.2_0.03_260)]">
      <div className="animate-blob-a absolute -left-1/4 top-[-15%] size-[65vmax] rounded-full bg-primary/60 blur-[110px]" />
      <div className="animate-blob-b absolute -right-1/4 top-[-5%] size-[60vmax] rounded-full bg-[oklch(0.42_0.09_260)]/70 blur-[110px]" />
      <div className="animate-blob-c absolute -bottom-1/3 left-[5%] size-[55vmax] rounded-full bg-[oklch(0.62_0.19_260)]/50 blur-[120px]" />
      <div className="animate-blob-d absolute -bottom-1/4 right-[5%] size-[50vmax] rounded-full bg-[oklch(0.67_0.15_42)]/50 blur-[120px]" />
      <div className="absolute inset-0 bg-gradient-to-b from-black/10 via-transparent to-black/50" />
      <div className="absolute inset-0 opacity-[0.03] mix-blend-overlay [background-image:url('data:image/svg+xml,%3Csvg xmlns=%22http://www.w3.org/2000/svg%22 width=%22100%22 height=%22100%22%3E%3Cfilter id=%22n%22%3E%3CfeTurbulence type=%22fractalNoise%22 baseFrequency=%220.9%22 numOctaves=%222%22 stitchTiles=%22stitch%22/%3E%3C/filter%3E%3Crect width=%22100%25%22 height=%22100%25%22 filter=%22url(%23n)%22/%3E%3C/svg%3E')]" />
    </div>
  )
}
