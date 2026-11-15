[out:json][timeout:60];
(
  way["highway"](52.0800,7.5960,52.0990,7.6260);
  way["waterway"](52.0800,7.5960,52.0990,7.6260);
  way["natural"="water"](52.0800,7.5960,52.0990,7.6260);
  relation["natural"="water"](52.0800,7.5960,52.0990,7.6260);
  way["building"](52.0800,7.5960,52.0990,7.6260);
  way["landuse"~"grass|meadow|recreation_ground|forest"](52.0800,7.5960,52.0990,7.6260);
  way["leisure"~"park|pitch|swimming_pool|sports_centre"](52.0800,7.5960,52.0990,7.6260);
  nwr["amenity"~"townhall|school"](52.0800,7.5960,52.0990,7.6260);
);
out geom;
