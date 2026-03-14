export interface CameraConfig {
  name: string;
  ip: string;
  url: string;
  group: string;
}

export interface CameraImage {
  url: string;
  date: string;
  timeAgo: string;
}

export interface PingResult {
  name: string;
  isOnline: boolean;
}
